'use client'

import React, { useEffect, useState } from 'react'

const Dashboard = () => {

    return (
        <div className='w-full h-full flex flex-col items-center justify-center pt-6 space-y-6'>
            <div className='border p-4 bg-yellow-50 rounded'>Плейсхолдер для уведомлений</div>
            <div className='border p-2 bg-blue-50 rounded'>
                Выбор периода
            </div>
            <div className='flex space-x-4'>
                <div className='p-6 border bg-green-50 rounded'>
                    Суммарные расходы за период: 25000 ₽
                </div>
                <div className='p-6 border bg-green-50 rounded'>
                Суммарные доходы за период: 50000 ₽
                </div>
            </div>
            <div className='p-6 border bg-purple-50 rounded w-full'>
                Баланс за период: 25000 ₽
            </div>
            <div className='flex flex-1 w-full gap-4'>
                <div className='flex-2 border bg-pink-50 h-64 flex flex-col items-center justify-start p-4 overflow-y-auto rounded'>
                    <div className='mb-4 text-center font-semibold'>
                        Список последних 5 транзакций за период
                    </div>
                    <ul className='space-y-2 w-full'>
                        <li className='p-2 bg-white border rounded'>Транзакция 1</li>
                        <li className='p-2 bg-white border rounded'>Транзакция 2</li>
                        <li className='p-2 bg-white border rounded'>Транзакция 3</li>
                        <li className='p-2 bg-white border rounded'>Транзакция 4</li>
                        <li className='p-2 bg-white border rounded'>Транзакция 5</li>
                    </ul>
                </div>
                <div className='flex-4 border bg-orange-50 h-64 flex items-center justify-center p-4 rounded'>
                    График/Диаграмма распределения расходов по категориям за период
                </div>
            </div>
            
        </div>
    )
}

export default Dashboard